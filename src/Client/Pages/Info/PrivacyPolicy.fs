namespace Pages

open Fable.Core
open Feliz
open Feliz.DaisyUI

// https://dsgvo-muster-datenschutzerklaerung.dg-datenschutz.de/#1487008989473-a9d4be68-00c7
type PrivacyPolicy =

    [<ReactComponent>]
    static member Main() =
        Html.div [
            prop.className "swt:prose-sm swt:md:prose swt:lg:prose-lg swt:py-1 swt:lg:py-4"
            prop.innerHtml
                $"""
                <h1>Privacy Policy</h1>

                <p>We are very delighted that you have shown interest in our enterprise. Data protection is of a particularly high priority for the management of the Computational Systems Biology. The use of the Internet pages of the Computational Systems Biology is possible without any indication of personal data; however, if a data subject wants to use special enterprise services via our website, processing of personal data could become necessary. If the processing of personal data is necessary and there is no statutory basis for such processing, we generally obtain consent from the data subject.</p>

                <p>The processing of personal data, such as the name, address, e-mail address, or telephone number of a data subject shall always be in line with the General Data Protection Regulation (GDPR), and in accordance with the country-specific data protection regulations applicable to the Computational Systems Biology. By means of this data protection declaration, our enterprise would like to inform the general public of the nature, scope, and purpose of the personal data we collect, use and process. Furthermore, data subjects are informed, by means of this data protection declaration, of the rights to which they are entitled.</p>

                <p>As the controller, the Computational Systems Biology has implemented numerous technical and organizational measures to ensure the most complete protection of personal data processed through this website. However, Internet-based data transmissions may in principle have security gaps, so absolute protection may not be guaranteed. For this reason, every data subject is free to transfer personal data to us via alternative means, e.g. by telephone. </p>

                <h4>1. Definitions</h4>
                <p>The data protection declaration of the Computational Systems Biology is based on the terms used by the European legislator for the adoption of the General Data Protection Regulation (GDPR). Our data protection declaration should be legible and understandable for the general public, as well as our customers and business partners. To ensure this, we would like to first explain the terminology used.</p>

                <p>In this data protection declaration, we use, inter alia, the following terms:</p>

                <ul>
                    <li>
                        <h6>a) Personal data</h6>
                        <p>Personal data means any information relating to an identified or identifiable natural person (“data subject”). An identifiable natural person is one who can be identified, directly or indirectly, in particular by reference to an identifier such as a name, an identification number, location data, an online identifier or to one or more factors specific to the physical, physiological, genetic, mental, economic, cultural or social identity of that natural person.</p>
                    </li>
                    <li>
                        <h6>b) Data subject</h6>
                        <p>Data subject is any identified or identifiable natural person, whose personal data is processed by the controller responsible for the processing.</p>
                    </li>
                    <li>
                        <h6>c) Processing</h6>
                        <p>Processing is any operation or set of operations which is performed on personal data or on sets of personal data, whether or not by automated means, such as collection, recording, organisation, structuring, storage, adaptation or alteration, retrieval, consultation, use, disclosure by transmission, dissemination or otherwise making available, alignment or combination, restriction, erasure or destruction. </p>
                    </li>
                    <li>
                        <h6>d) Restriction of processing</h6>
                        <p>Restriction of processing is the marking of stored personal data with the aim of limiting their processing in the future. </p>
                    </li>
                    <li>
                        <h6>e) Profiling</h6>
                        <p>Profiling means any form of automated processing of personal data consisting of the use of personal data to evaluate certain personal aspects relating to a natural person, in particular to analyse or predict aspects concerning that natural person's performance at work, economic situation, health, personal preferences, interests, reliability, behaviour, location or movements. </p>
                    </li>
                    <li>
                        <h6>f) Pseudonymisation</h6>
                        <p>Pseudonymisation is the processing of personal data in such a manner that the personal data can no longer be attributed to a specific data subject without the use of additional information, provided that such additional information is kept separately and is subject to technical and organisational measures to ensure that the personal data are not attributed to an identified or identifiable natural person. </p>
                    </li>
                    <li>
                        <h6>g) Controller or controller responsible for the processing</h6>
                        <p>Controller or controller responsible for the processing is the natural or legal person, public authority, agency or other body which, alone or jointly with others, determines the purposes and means of the processing of personal data; where the purposes and means of such processing are determined by Union or Member State law, the controller or the specific criteria for its nomination may be provided for by Union or Member State law. </p>
                    </li>
                    <li>
                        <h6>h) Processor</h6>
                        <p>Processor is a natural or legal person, public authority, agency or other body which processes personal data on behalf of the controller. </p>
                    </li>
                    <li>
                        <h6>i) Recipient</h6>
                        <p>Recipient is a natural or legal person, public authority, agency or another body, to which the personal data are disclosed, whether a third party or not. However, public authorities which may receive personal data in the framework of a particular inquiry in accordance with Union or Member State law shall not be regarded as recipients; the processing of those data by those public authorities shall be in compliance with the applicable data protection rules according to the purposes of the processing. </p>
                    </li>
                    <li>
                        <h6>j) Third party</h6>
                        <p>Third party is a natural or legal person, public authority, agency or body other than the data subject, controller, processor and persons who, under the direct authority of the controller or processor, are authorised to process personal data.</p>
                    </li>
                    <li>
                        <h6>k) Consent</h6>
                        <p>Consent of the data subject is any freely given, specific, informed and unambiguous indication of the data subject's wishes by which he or she, by a statement or by a clear affirmative action, signifies agreement to the processing of personal data relating to him or her. </p>
                    </li>
                </ul>

                <h4>2. Name and Address of the controller</h4>
                <p>
                    Controller for the purposes of the General Data Protection Regulation (GDPR), other data protection laws applicable in Member states of the European Union and other provisions related to data protection is:

                </p>

                <p>Computational Systems Biology</p>
                <p>Erwin-Schrödinger-Str. 56 R244</p>
                <p>67663 Kaiserslautern</p>
                <p>Germany</p>
                <p>Phone: 49 631 205 4657</p>
                <p>Email: timo.muehlhaus@rptu.de</p>
                <p>Website: https://swate-alpha.nfdi4plants.org</p>

                <h4>3. Collection of general data and information</h4>
                <p>The website of the Computational Systems Biology collects a series of general data and information when a data subject or automated system calls up the website. This general data and information are stored in the server log files. Collected may be (1) the browser types and versions used, (2) the operating system used by the accessing system, (3) the website from which an accessing system reaches our website (so-called referrers), (4) the sub-websites, (5) the date and time of access to the Internet site, (6) an Internet protocol address (IP address), (7) the Internet service provider of the accessing system, and (8) any other similar data and information that may be used in the event of attacks on our information technology systems.</p>

                <p>When using these general data and information, the Computational Systems Biology does not draw any conclusions about the data subject. Rather, this information is needed to (1) deliver the content of our website correctly, (2) optimize the content of our website as well as its advertisement, (3) ensure the long-term viability of our information technology systems and website technology, and (4) provide law enforcement authorities with the information necessary for criminal prosecution in case of a cyber-attack. Therefore, the Computational Systems Biology analyzes anonymously collected data and information statistically, with the aim of increasing the data protection and data security of our enterprise, and to ensure an optimal level of protection for the personal data we process. The anonymous data of the server log files are stored separately from all personal data provided by a data subject.</p>

                <h4>4. Contact possibility via the website </h4>
                <p>The website of the Computational Systems Biology contains information that enables a quick electronic contact to our enterprise, as well as direct communication with us, which also includes a general address of the so-called electronic mail (e-mail address). If a data subject contacts the controller by e-mail or via a contact form, the personal data transmitted by the data subject are automatically stored. Such personal data transmitted on a voluntary basis by a data subject to the data controller are stored for the purpose of processing or contacting the data subject. There is no transfer of this personal data to third parties.</p>

                <h4>5. Routine erasure and blocking of personal data</h4>
                <p>The data controller shall process and store the personal data of the data subject only for the period necessary to achieve the purpose of storage, or as far as this is granted by the European legislator or other legislators in laws or regulations to which the controller is subject to.</p>

                <p>If the storage purpose is not applicable, or if a storage period prescribed by the European legislator or another competent legislator expires, the personal data are routinely blocked or erased in accordance with legal requirements.</p>

                <h4>6. Rights of the data subject</h4>
                <ul style=list-style: none>
                    <li>
                        <h6>a) Right of confirmation</h6>
                        <p>Each data subject shall have the right granted by the European legislator to obtain from the controller the confirmation as to whether or not personal data concerning him or her are being processed. If a data subject wishes to avail himself of this right of confirmation, he or she may, at any time, contact any employee of the controller.</p>
                    </li>
                    <li>
                        <h6>b) Right of access</h6>
                        <p>Each data subject shall have the right granted by the European legislator to obtain from the controller free information about his or her personal data stored at any time and a copy of this information. Furthermore, the European directives and regulations grant the data subject access to the following information:</p>

                        <ul style=list-style: none>
                            <li>the purposes of the processing;</li>
                            <li>the categories of personal data concerned;</li>
                            <li>the recipients or categories of recipients to whom the personal data have been or will be disclosed, in particular recipients in third countries or international organisations;</li>
                            <li>where possible, the envisaged period for which the personal data will be stored, or, if not possible, the criteria used to determine that period;</li>
                            <li>the existence of the right to request from the controller rectification or erasure of personal data, or restriction of processing of personal data concerning the data subject, or to object to such processing;</li>
                            <li>the existence of the right to lodge a complaint with a supervisory authority;</li>
                            <li>where the personal data are not collected from the data subject, any available information as to their source;</li>
                            <li>the existence of automated decision-making, including profiling, referred to in Article 22(1) and (4) of the GDPR and, at least in those cases, meaningful information about the logic involved, as well as the significance and envisaged consequences of such processing for the data subject.</li>

                        </ul>
                        <p>Furthermore, the data subject shall have a right to obtain information as to whether personal data are transferred to a third country or to an international organisation. Where this is the case, the data subject shall have the right to be informed of the appropriate safeguards relating to the transfer.</p>

                        <p>If a data subject wishes to avail himself of this right of access, he or she may, at any time, contact any employee of the controller.</p>
                    </li>
                    <li>
                        <h6>c) Right to rectification </h6>
                        <p>Each data subject shall have the right granted by the European legislator to obtain from the controller without undue delay the rectification of inaccurate personal data concerning him or her. Taking into account the purposes of the processing, the data subject shall have the right to have incomplete personal data completed, including by means of providing a supplementary statement.</p>

                        <p>If a data subject wishes to exercise this right to rectification, he or she may, at any time, contact any employee of the controller.</p>
                    </li>
                    <li>
                        <h6>d) Right to erasure (Right to be forgotten) </h6>
                        <p>Each data subject shall have the right granted by the European legislator to obtain from the controller the erasure of personal data concerning him or her without undue delay, and the controller shall have the obligation to erase personal data without undue delay where one of the following grounds applies, as long as the processing is not necessary: </p>

                        <ul style=list-style: none>
                            <li>The personal data are no longer necessary in relation to the purposes for which they were collected or otherwise processed.</li>
                            <li>The data subject withdraws consent to which the processing is based according to point (a) of Article 6(1) of the GDPR, or point (a) of Article 9(2) of the GDPR, and where there is no other legal ground for the processing.</li>
                            <li>The data subject objects to the processing pursuant to Article 21(1) of the GDPR and there are no overriding legitimate grounds for the processing, or the data subject objects to the processing pursuant to Article 21(2) of the GDPR. </li>
                            <li>The personal data have been unlawfully processed.</li>
                            <li>The personal data must be erased for compliance with a legal obligation in Union or Member State law to which the controller is subject.</li>
                            <li>The personal data have been collected in relation to the offer of information society services referred to in Article 8(1) of the GDPR.</li>

                        </ul>
                        <p>If one of the aforementioned reasons applies, and a data subject wishes to request the erasure of personal data stored by the Computational Systems Biology, he or she may, at any time, contact any employee of the controller. An employee of Computational Systems Biology shall promptly ensure that the erasure request is complied with immediately.</p>

                        <p>Where the controller has made personal data public and is obliged pursuant to Article 17(1) to erase the personal data, the controller, taking account of available technology and the cost of implementation, shall take reasonable steps, including technical measures, to inform other controllers processing the personal data that the data subject has requested erasure by such controllers of any links to, or copy or replication of, those personal data, as far as processing is not required. An employees of the Computational Systems Biology will arrange the necessary measures in individual cases.</p>
                    </li>
                    <li>
                        <h6>e) Right of restriction of processing</h6>
                        <p>Each data subject shall have the right granted by the European legislator to obtain from the controller restriction of processing where one of the following applies:</p>

                        <ul style=list-style: none>
                            <li>The accuracy of the personal data is contested by the data subject, for a period enabling the controller to verify the accuracy of the personal data. </li>
                            <li>The processing is unlawful and the data subject opposes the erasure of the personal data and requests instead the restriction of their use instead.</li>
                            <li>The controller no longer needs the personal data for the purposes of the processing, but they are required by the data subject for the establishment, exercise or defence of legal claims.</li>
                            <li>The data subject has objected to processing pursuant to Article 21(1) of the GDPR pending the verification whether the legitimate grounds of the controller override those of the data subject.</li>

                        </ul>
                        <p>If one of the aforementioned conditions is met, and a data subject wishes to request the restriction of the processing of personal data stored by the Computational Systems Biology, he or she may at any time contact any employee of the controller. The employee of the Computational Systems Biology will arrange the restriction of the processing. </p>
                    </li>
                    <li>
                        <h6>f) Right to data portability</h6>
                        <p>Each data subject shall have the right granted by the European legislator, to receive the personal data concerning him or her, which was provided to a controller, in a structured, commonly used and machine-readable format. He or she shall have the right to transmit those data to another controller without hindrance from the controller to which the personal data have been provided, as long as the processing is based on consent pursuant to point (a) of Article 6(1) of the GDPR or point (a) of Article 9(2) of the GDPR, or on a contract pursuant to point (b) of Article 6(1) of the GDPR, and the processing is carried out by automated means, as long as the processing is not necessary for the performance of a task carried out in the public interest or in the exercise of official authority vested in the controller.</p>

                        <p>Furthermore, in exercising his or her right to data portability pursuant to Article 20(1) of the GDPR, the data subject shall have the right to have personal data transmitted directly from one controller to another, where technically feasible and when doing so does not adversely affect the rights and freedoms of others.</p>

                        <p>In order to assert the right to data portability, the data subject may at any time contact any employee of the Computational Systems Biology.</p>

                    </li>
                    <li>
                        <h6>g) Right to object</h6>
                        <p>Each data subject shall have the right granted by the European legislator to object, on grounds relating to his or her particular situation, at any time, to processing of personal data concerning him or her, which is based on point (e) or (f) of Article 6(1) of the GDPR. This also applies to profiling based on these provisions.</p>

                        <p>The Computational Systems Biology shall no longer process the personal data in the event of the objection, unless we can demonstrate compelling legitimate grounds for the processing which override the interests, rights and freedoms of the data subject, or for the establishment, exercise or defence of legal claims.</p>

                        <p>If the Computational Systems Biology processes personal data for direct marketing purposes, the data subject shall have the right to object at any time to processing of personal data concerning him or her for such marketing. This applies to profiling to the extent that it is related to such direct marketing. If the data subject objects to the Computational Systems Biology to the processing for direct marketing purposes, the Computational Systems Biology will no longer process the personal data for these purposes.</p>

                        <p>In addition, the data subject has the right, on grounds relating to his or her particular situation, to object to processing of personal data concerning him or her by the Computational Systems Biology for scientific or historical research purposes, or for statistical purposes pursuant to Article 89(1) of the GDPR, unless the processing is necessary for the performance of a task carried out for reasons of public interest.</p>

                        <p>In order to exercise the right to object, the data subject may contact any employee of the Computational Systems Biology. In addition, the data subject is free in the context of the use of information society services, and notwithstanding Directive 2002/58/EC, to use his or her right to object by automated means using technical specifications.</p>
                    </li>
                    <li>
                        <h6>h) Automated individual decision-making, including profiling</h6>
                        <p>Each data subject shall have the right granted by the European legislator not to be subject to a decision based solely on automated processing, including profiling, which produces legal effects concerning him or her, or similarly significantly affects him or her, as long as the decision (1) is not is necessary for entering into, or the performance of, a contract between the data subject and a data controller, or (2) is not authorised by Union or Member State law to which the controller is subject and which also lays down suitable measures to safeguard the data subject's rights and freedoms and legitimate interests, or (3) is not based on the data subject's explicit consent.</p>

                        <p>If the decision (1) is necessary for entering into, or the performance of, a contract between the data subject and a data controller, or (2) it is based on the data subject's explicit consent, the Computational Systems Biology shall implement suitable measures to safeguard the data subject's rights and freedoms and legitimate interests, at least the right to obtain human intervention on the part of the controller, to express his or her point of view and contest the decision.</p>

                        <p>If the data subject wishes to exercise the rights concerning automated individual decision-making, he or she may, at any time, contact any employee of the Computational Systems Biology.</p>

                    </li>
                    <li>
                        <h6>i) Right to withdraw data protection consent </h6>
                        <p>Each data subject shall have the right granted by the European legislator to withdraw his or her consent to processing of his or her personal data at any time. </p>

                        <p>If the data subject wishes to exercise the right to withdraw the consent, he or she may, at any time, contact any employee of the Computational Systems Biology.</p>

                    </li>
                </ul>
                <h4>7. Data protection for applications and the application procedures</h4>
                <p>The data controller shall collect and process the personal data of applicants for the purpose of the processing of the application procedure. The processing may also be carried out electronically. This is the case, in particular, if an applicant submits corresponding application documents by e-mail or by means of a web form on the website to the controller. If the data controller concludes an employment contract with an applicant, the submitted data will be stored for the purpose of processing the employment relationship in compliance with legal requirements. If no employment contract is concluded with the applicant by the controller, the application documents shall be automatically erased two months after notification of the refusal decision, provided that no other legitimate interests of the controller are opposed to the erasure. Other legitimate interest in this relation is, e.g. a burden of proof in a procedure under the General Equal Treatment Act (AGG).</p>

                <h4>8. Data protection provisions about the application and use of Twitter</h4>
                <p>On this website, the controller has integrated components of Twitter. Twitter is a multilingual, publicly-accessible microblogging service on which users may publish and spread so-called ‘tweets,’ e.g. short messages, which are limited to 280 characters. These short messages are available for everyone, including those who are not logged on to Twitter. The tweets are also displayed to so-called followers of the respective user. Followers are other Twitter users who follow a user's tweets. Furthermore, Twitter allows you to address a wide audience via hashtags, links or retweets.</p>

                <p>The operating company of Twitter is Twitter International Company, One Cumberland Place, Fenian Street Dublin 2, D02 AX07, Ireland.</p>

                <p>With each call-up to one of the individual pages of this Internet site, which is operated by the controller and on which a Twitter component (Twitter button) was integrated, the Internet browser on the information technology system of the data subject is automatically prompted to download a display of the corresponding Twitter component of Twitter. Further information about the Twitter buttons is available under https://about.twitter.com/de/resources/buttons. During the course of this technical procedure, Twitter gains knowledge of what specific sub-page of our website was visited by the data subject. The purpose of the integration of the Twitter component is a retransmission of the contents of this website to allow our users to introduce this web page to the digital world and increase our visitor numbers.</p>

                <p>If the data subject is logged in at the same time on Twitter, Twitter detects with every call-up to our website by the data subject and for the entire duration of their stay on our Internet site which specific sub-page of our Internet page was visited by the data subject. This information is collected through the Twitter component and associated with the respective Twitter account of the data subject. If the data subject clicks on one of the Twitter buttons integrated on our website, then Twitter assigns this information to the personal Twitter user account of the data subject and stores the personal data.</p>

                <p>Twitter receives information via the Twitter component that the data subject has visited our website, provided that the data subject is logged in on Twitter at the time of the call-up to our website. This occurs regardless of whether the person clicks on the Twitter component or not. If such a transmission of information to Twitter is not desirable for the data subject, then he or she may prevent this by logging off from their Twitter account before a call-up to our website is made.</p>

                <p>The applicable data protection provisions of Twitter may be accessed under https://twitter.com/privacy?lang=en.</p>

                <h4>9. Data protection provisions about the application and use of YouTube</h4>
                <p>
                    On this website, the controller has integrated components of YouTube. YouTube is an Internet video portal that enables video publishers to set video clips and other users free of charge, which also provides free viewing, review and commenting on them. YouTube allows you to publish all kinds of videos, so you can access both full movies and TV broadcasts, as well as music videos, trailers, and videos made by users via the Internet portal.
                </p>

                <p>The operating company of YouTube is Google Ireland Limited, Gordon House, Barrow Street, Dublin, D04 E5W5, Ireland.</p>

                <p>With each call-up to one of the individual pages of this Internet site, which is operated by the controller and on which a YouTube component (YouTube video) was integrated, the Internet browser on the information technology system of the data subject is automatically prompted to download a display of the corresponding YouTube component. Further information about YouTube may be obtained under https://www.youtube.com/yt/about/en/. During the course of this technical procedure, YouTube and Google gain knowledge of what specific sub-page of our website was visited by the data subject.</p>

                <p>If the data subject is logged in on YouTube, YouTube recognizes with each call-up to a sub-page that contains a YouTube video, which specific sub-page of our Internet site was visited by the data subject. This information is collected by YouTube and Google and assigned to the respective YouTube account of the data subject.</p>

                <p>YouTube and Google will receive information through the YouTube component that the data subject has visited our website, if the data subject at the time of the call to our website is logged in on YouTube; this occurs regardless of whether the person clicks on a YouTube video or not. If such a transmission of this information to YouTube and Google is not desirable for the data subject, the delivery may be prevented if the data subject logs off from their own YouTube account before a call-up to our website is made.</p>

                <p>YouTube's data protection provisions, available at https://www.google.com/intl/en/policies/privacy/, provide information about the collection, processing and use of personal data by YouTube and Google.</p>

                <h4>10. Legal basis for the processing </h4>
                <p>
                    Art. 6(1) lit. a GDPR serves as the legal basis for processing operations for which we obtain consent for a specific processing purpose. If the processing of personal data is necessary for the performance of a contract to which the data subject is party, as is the case, for example, when processing operations are necessary for the supply of goods or to provide any other service, the processing is based on Article 6(1) lit. b GDPR. The same applies to such processing operations which are necessary for carrying out pre-contractual measures, for example in the case of inquiries concerning our products or services. Is our company subject to a legal obligation by which processing of personal data is required, such as for the fulfillment of tax obligations, the processing is based on Art. 6(1) lit. c GDPR.
                    In rare cases, the processing of personal data may be necessary to protect the vital interests of the data subject or of another natural person. This would be the case, for example, if a visitor were injured in our company and his name, age, health insurance data or other vital information would have to be passed on to a doctor, hospital or other third party. Then the processing would be based on Art. 6(1) lit. d GDPR.
                    Finally, processing operations could be based on Article 6(1) lit. f GDPR. This legal basis is used for processing operations which are not covered by any of the abovementioned legal grounds, if processing is necessary for the purposes of the legitimate interests pursued by our company or by a third party, except where such interests are overridden by the interests or fundamental rights and freedoms of the data subject which require protection of personal data. Such processing operations are particularly permissible because they have been specifically mentioned by the European legislator. He considered that a legitimate interest could be assumed if the data subject is a client of the controller (Recital 47 Sentence 2 GDPR).
                </p>

                <h4>11. The legitimate interests pursued by the controller or by a third party</h4>
                <p>Where the processing of personal data is based on Article 6(1) lit. f GDPR our legitimate interest is to carry out our business in favor of the well-being of all our employees and the shareholders.</p>

                <h4>12. Period for which the personal data will be stored</h4>
                <p>The criteria used to determine the period of storage of personal data is the respective statutory retention period. After expiration of that period, the corresponding data is routinely deleted, as long as it is no longer necessary for the fulfillment of the contract or the initiation of a contract.</p>

                <h4>13. Provision of personal data as statutory or contractual requirement; Requirement necessary to enter into a contract; Obligation of the data subject to provide the personal data; possible consequences of failure to provide such data </h4>
                <p>
                    We clarify that the provision of personal data is partly required by law (e.g. tax regulations) or can also result from contractual provisions (e.g. information on the contractual partner).

                    Sometimes it may be necessary to conclude a contract that the data subject provides us with personal data, which must subsequently be processed by us. The data subject is, for example, obliged to provide us with personal data when our company signs a contract with him or her. The non-provision of the personal data would have the consequence that the contract with the data subject could not be concluded.

                    Before personal data is provided by the data subject, the data subject must contact any employee. The employee clarifies to the data subject whether the provision of the personal data is required by law or contract or is necessary for the conclusion of the contract, whether there is an obligation to provide the personal data and the consequences of non-provision of the personal data.
                </p>

                <h4>14. Existence of automated decision-making</h4>
                <p>As a responsible company, we do not use automatic decision-making or profiling.</p>

                <p>Developed by the specialists for <a href="https://willing-able.com/">LegalTech</a> at Willing & Able that also developed the system for <a href="https://abletorecords.com/">DPIA</a>. The legal texts contained in our privacy policy generator have been provided and published by <a href="https://dg-datenschutz.de/">Prof. Dr. h.c. Heiko Jonny Maniero</a> from the German Association for Data Protection and <a href="https://www.wbs-law.de/" rel="nofollow">Christian Solmecke</a> from WBS law.</p>
            """
        ]